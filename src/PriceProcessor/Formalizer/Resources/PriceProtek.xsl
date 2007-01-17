<?xml version='1.0' encoding = "windows-1251" standalone = "yes"?>

<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">

	<xsl:output method="xml" indent="yes" encoding="windows-1251" />

	<xsl:template match="/" >
		<Price>

			<xsl:for-each select="����������������������/����������������/�����������/�����������">
				<Position>
					<Code>
						<xsl:value-of select="��"/>
					</Code>
					<Name1>
						<xsl:value-of select="������������"/>
					</Name1>
					<FirmCr>
						<xsl:value-of select="������������/�����������������������"/>
					</FirmCr>
					<Period>
						<xsl:value-of select="���������������/����������������[child::�� = 'EXP_DATE']/��������"/>
					</Period>
					<Volume>
						<xsl:value-of select="���������������/����������������[child::�� = 'SPACK']/��������"/>
					</Volume>
					<Quantity>
						<xsl:value-of select="���������������/����������������[child::�� = 'QTY']/��������"/>
					</Quantity>
					<xsl:for-each select="����/����">
						<xsl:element name="Cost{����������}">
							<xsl:value-of select="�������������"/>
						</xsl:element>
					</xsl:for-each>

					</Position>
			</xsl:for-each>

		</Price>
	</xsl:template>

</xsl:stylesheet>
